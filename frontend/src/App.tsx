import { useCallback, useEffect, useMemo, useState } from "react";
import type { FormEvent } from "react";
import { admissionApi, ApiError } from "./api";
import type {
  AdmissionClassSummary,
  StudentHold,
} from "./api";

const STUDENT_ID_KEY = "school-admission.student-id";
const REFRESH_INTERVAL_MS = 5_000;

function readSavedStudentId(): string {
  try {
    return window.localStorage.getItem(STUDENT_ID_KEY) ?? "";
  } catch {
    return "";
  }
}

function formatRemainingTime(expiresAt: string, now: number): string {
  const remainingMs = new Date(expiresAt).getTime() - now;
  if (!Number.isFinite(remainingMs) || remainingMs <= 0) {
    return "Expired";
  }

  const totalSeconds = Math.ceil(remainingMs / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return minutes > 0
    ? `${minutes}m ${seconds.toString().padStart(2, "0")}s left`
    : `${seconds}s left`;
}

function currentHoldStatus(hold: StudentHold, now: number): string {
  if (hold.status === "HELD" && new Date(hold.expiresAt).getTime() <= now) {
    return "EXPIRED";
  }
  return hold.status;
}

function errorMessage(error: unknown): string {
  return error instanceof ApiError
    ? error.message
    : "Something went wrong. Please try again.";
}

export default function App() {
  const [studentId, setStudentId] = useState(readSavedStudentId);
  const [classes, setClasses] = useState<AdmissionClassSummary[]>([]);
  const [holds, setHolds] = useState<StudentHold[]>([]);
  const [className, setClassName] = useState("");
  const [totalSeats, setTotalSeats] = useState("");
  const [now, setNow] = useState(Date.now());
  const [classesLoading, setClassesLoading] = useState(true);
  const [holdsLoading, setHoldsLoading] = useState(
    () => Boolean(readSavedStudentId().trim()),
  );
  const [classError, setClassError] = useState("");
  const [holdsError, setHoldsError] = useState("");
  const [actionError, setActionError] = useState("");
  const [notice, setNotice] = useState("");
  const [busyAction, setBusyAction] = useState("");
  const normalizedStudentId = studentId.trim();

  const refreshClasses = useCallback(async () => {
    try {
      const results = await admissionApi.getClasses();
      setClasses(results);
      setClassError("");
    } catch (error) {
      setClassError(errorMessage(error));
    } finally {
      setClassesLoading(false);
    }
  }, []);

  const refreshHolds = useCallback(async () => {
    if (!normalizedStudentId) {
      setHolds([]);
      setHoldsLoading(false);
      setHoldsError("");
      return;
    }

    setHoldsLoading(true);
    try {
      const results = await admissionApi.getStudentHolds(normalizedStudentId);
      setHolds(results);
      setHoldsError("");
    } catch (error) {
      setHoldsError(errorMessage(error));
    } finally {
      setHoldsLoading(false);
    }
  }, [normalizedStudentId]);

  useEffect(() => {
    void refreshClasses();
    const intervalId = window.setInterval(() => {
      void refreshClasses();
    }, REFRESH_INTERVAL_MS);
    return () => window.clearInterval(intervalId);
  }, [refreshClasses]);

  useEffect(() => {
    if (!normalizedStudentId) {
      void refreshHolds();
      return;
    }

    const initialRequest = window.setTimeout(() => {
      void refreshHolds();
    }, 250);
    const intervalId = window.setInterval(() => {
      void refreshHolds();
    }, REFRESH_INTERVAL_MS);
    return () => {
      window.clearTimeout(initialRequest);
      window.clearInterval(intervalId);
    };
  }, [normalizedStudentId, refreshHolds]);

  useEffect(() => {
    const intervalId = window.setInterval(() => setNow(Date.now()), 1_000);
    return () => window.clearInterval(intervalId);
  }, []);

  const activeApplications = useMemo(
    () => holds.filter((hold) => ["HELD", "CONFIRMED"].includes(
      currentHoldStatus(hold, now),
    )),
    [holds, now],
  );
  const visibleApplications = useMemo(
    () => holds.filter((hold) => ["HELD", "CONFIRMED", "EXPIRED"].includes(
      currentHoldStatus(hold, now),
    )),
    [holds, now],
  );
  const pastApplications = useMemo(
    () => holds.filter((hold) => {
      const status = currentHoldStatus(hold, now);
      return status !== "HELD" && status !== "CONFIRMED" && status !== "EXPIRED";
    }),
    [holds, now],
  );

  function updateStudentId(value: string) {
    setStudentId(value);
    setActionError("");
    setNotice("");
    try {
      if (value.trim()) {
        window.localStorage.setItem(STUDENT_ID_KEY, value);
      } else {
        window.localStorage.removeItem(STUDENT_ID_KEY);
      }
    } catch {
      setActionError("Your browser could not save this registration number.");
    }
  }

  async function handleCreateClass(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setActionError("");
    setNotice("");
    const capacity = Number(totalSeats);
    if (!className.trim() || !Number.isInteger(capacity) || capacity <= 0) {
      setActionError("Enter a class name and a positive whole-number seat capacity.");
      return;
    }

    setBusyAction("create-class");
    try {
      await admissionApi.createClass(className.trim(), capacity);
      setClassName("");
      setTotalSeats("");
      setNotice("Class created successfully.");
      await refreshClasses();
    } catch (error) {
      setActionError(errorMessage(error));
    } finally {
      setBusyAction("");
    }
  }

  async function handleApply(admissionClass: AdmissionClassSummary) {
    if (!normalizedStudentId) {
      setActionError("Enter your student registration number before applying.");
      document.getElementById("student-id")?.focus();
      return;
    }

    setBusyAction(`apply:${admissionClass.id}`);
    setActionError("");
    setNotice("");
    try {
      await admissionApi.createHold(admissionClass.id, normalizedStudentId);
      setNotice(`Your seat in ${admissionClass.name} is on hold. Complete payment before it expires.`);
      await Promise.all([refreshClasses(), refreshHolds()]);
    } catch (error) {
      setActionError(errorMessage(error));
      await Promise.all([refreshClasses(), refreshHolds()]);
    } finally {
      setBusyAction("");
    }
  }

  async function handleConfirm(hold: StudentHold) {
    if (!normalizedStudentId) {
      setActionError("Enter your student registration number to continue.");
      return;
    }

    setBusyAction(`confirm:${hold.holdId}`);
    setActionError("");
    setNotice("");
    try {
      await admissionApi.confirmHold(hold.holdId, normalizedStudentId);
      setNotice("Admission fee payment successful. Your seat is confirmed.");
      await Promise.all([refreshClasses(), refreshHolds()]);
    } catch (error) {
      setActionError(errorMessage(error));
      await Promise.all([refreshClasses(), refreshHolds()]);
    } finally {
      setBusyAction("");
    }
  }

  async function handleCancel(hold: StudentHold) {
    if (!normalizedStudentId) {
      setActionError("Enter your student registration number to continue.");
      return;
    }

    setBusyAction(`cancel:${hold.holdId}`);
    setActionError("");
    setNotice("");
    try {
      await admissionApi.cancelHold(hold.holdId, normalizedStudentId);
      setNotice(`Your hold for ${hold.className} was cancelled.`);
      await Promise.all([refreshClasses(), refreshHolds()]);
    } catch (error) {
      setActionError(errorMessage(error));
      await Promise.all([refreshClasses(), refreshHolds()]);
    } finally {
      setBusyAction("");
    }
  }

  return (
    <main>
      <header className="topbar">
        <a className="brand" href="#" aria-label="School admissions home">
          <span className="brand-mark" aria-hidden="true">SA</span>
          <span>School Admissions</span>
        </a>
        <nav className="topnav" aria-label="Main navigation">
          <a href="#classes">Classes</a>
          <a href="#applications">My applications</a>
          <a href="#school-admin">For schools</a>
        </nav>
        <div className="identity-control">
          <label htmlFor="student-id">Student registration number</label>
          <input
            id="student-id"
            autoComplete="off"
            value={studentId}
            onChange={(event) => updateStudentId(event.target.value)}
            placeholder="Enter your registration number"
          />
          <span className="identity-hint">Saved on this device</span>
        </div>
      </header>

      <section className="hero">
        <div className="hero-copy">
          <p className="eyebrow">YOUR NEXT CHAPTER STARTS HERE</p>
          <h1>Find your place.<br /><span>Make it official.</span></h1>
          <p className="hero-description">
            Explore available classes, reserve your seat, and complete your
            admission in a few simple steps.
          </p>
          <a className="hero-link" href="#classes">
            Explore classes <span aria-hidden="true">↓</span>
          </a>
        </div>
        <div className="hero-art" aria-hidden="true">
          <div className="art-sun" />
          <div className="art-orbit orbit-one" />
          <div className="art-orbit orbit-two" />
          <div className="art-card">
            <div className="art-card-head"><span>ADMISSION</span><span>2026</span></div>
            <div className="art-seal">S</div>
            <div className="art-line wide" />
            <div className="art-line" />
            <div className="art-bottom"><span>YOUR FUTURE</span><span>IS WAITING</span></div>
          </div>
          <span className="art-spark spark-one">✳</span>
          <span className="art-spark spark-two">✳</span>
        </div>
        <div className="hero-footer">
          <span>01 — 03</span>
          <div className="hero-progress"><span /></div>
          <span>START YOUR APPLICATION</span>
        </div>
      </section>

      <section className="content-section" id="classes">
        <div className="section-heading">
          <div>
            <p className="eyebrow">CHOOSE YOUR CLASS</p>
            <h2>Open admissions</h2>
          </div>
          <p className="section-aside">Seat availability refreshes automatically.</p>
        </div>

        {actionError && <div className="alert alert-error" role="alert">{actionError}</div>}
        {notice && <div className="alert alert-success" role="status">{notice}</div>}
        {classError && (
          <div className="alert alert-error" role="alert">
            {classError} <button className="text-button" onClick={() => void refreshClasses()}>Retry</button>
          </div>
        )}

        <div className="class-grid">
          {classesLoading && classes.length === 0 && (
            <div className="empty-state">Loading available classes...</div>
          )}
          {!classesLoading && classes.length === 0 && !classError && (
            <div className="empty-state">No classes are open yet. Check back soon.</div>
          )}
          {classes.map((admissionClass, index) => {
            const alreadyApplied = holds.some(
              (hold) => hold.classId === admissionClass.id &&
                ["HELD", "CONFIRMED"].includes(currentHoldStatus(hold, now)),
            );
            const applying = busyAction === `apply:${admissionClass.id}`;
            return (
              <article className="class-card" key={admissionClass.id}>
                <div className="class-card-top">
                  <span className={`class-index tone-${index % 3}`}>
                    {String(index + 1).padStart(2, "0")}
                  </span>
                  <span className={`availability ${admissionClass.availableSeats > 0 ? "" : "full"}`}>
                    <span className="availability-dot" />
                    {admissionClass.availableSeats > 0 ? "Applications open" : "Class full"}
                  </span>
                </div>
                <h3>{admissionClass.name}</h3>
                <div className="seat-summary">
                  <div><strong>{admissionClass.totalSeats}</strong><span>Total seats</span></div>
                  <div><strong>{admissionClass.heldSeats}</strong><span>On hold</span></div>
                  <div><strong>{admissionClass.confirmedSeats}</strong><span>Confirmed</span></div>
                  <div className="available-count">
                    <strong>{admissionClass.availableSeats}</strong><span>Available</span>
                  </div>
                </div>
                <div className="seat-meter" aria-label={`${admissionClass.availableSeats} of ${admissionClass.totalSeats} seats available`}>
                  <span style={{
                    width: `${Math.min(100, Math.max(0,
                      (admissionClass.availableSeats / admissionClass.totalSeats) * 100))}%`,
                  }} />
                </div>
                <button
                  className="button button-dark apply-button"
                  disabled={
                    admissionClass.availableSeats <= 0 ||
                    alreadyApplied ||
                    applying ||
                    !normalizedStudentId
                  }
                  onClick={() => void handleApply(admissionClass)}
                  title={!normalizedStudentId ? "Enter your registration number first" : undefined}
                >
                  {applying
                    ? "Reserving..."
                    : alreadyApplied
                      ? "Application already exists"
                      : admissionClass.availableSeats <= 0
                        ? "No seats available"
                        : "Apply for this class"}
                  {!applying && !alreadyApplied && admissionClass.availableSeats > 0 && (
                    <span aria-hidden="true">↗</span>
                  )}
                </button>
              </article>
            );
          })}
        </div>
      </section>

      <section className="applications-section" id="applications">
        <div className="section-heading">
          <div>
            <p className="eyebrow">YOUR ADMISSION JOURNEY</p>
            <h2>My applications</h2>
          </div>
          <span className="application-count">
            {activeApplications.length.toString().padStart(2, "0")} ACTIVE
          </span>
        </div>

        {!normalizedStudentId ? (
          <div className="application-empty">
            <span className="empty-icon" aria-hidden="true">01</span>
            <div>
              <h3>Start with your registration number</h3>
              <p>Enter it at the top of the page to view and manage your applications.</p>
            </div>
          </div>
        ) : holdsLoading && holds.length === 0 ? (
          <div className="application-empty"><p>Loading your applications...</p></div>
        ) : holdsError ? (
          <div className="alert alert-error" role="alert">
            {holdsError} <button className="text-button" onClick={() => void refreshHolds()}>Retry</button>
          </div>
        ) : holds.length === 0 ? (
          <div className="application-empty">
            <span className="empty-icon" aria-hidden="true">+</span>
            <div>
              <h3>No applications yet</h3>
              <p>Choose an open class above to reserve your seat.</p>
            </div>
          </div>
        ) : (
          <div className="application-list">
            {visibleApplications.map((hold) => {
              const status = currentHoldStatus(hold, now);
              const isHeld = status === "HELD";
              const expired = status === "EXPIRED";
              const confirming = busyAction === `confirm:${hold.holdId}`;
              const cancelling = busyAction === `cancel:${hold.holdId}`;
              return (
                <article className="application-row" key={hold.holdId}>
                  <div className="application-class">
                    <span className="application-symbol" aria-hidden="true">A</span>
                    <div><h3>{hold.className}</h3><span>{hold.classId}</span></div>
                  </div>
                  <div className="application-status">
                    <span className={`status-pill status-${status.toLowerCase()}`}>
                      <span className="status-dot" />{status}
                    </span>
                  </div>
                  <div className="application-expiry">
                    <span>
                      {isHeld
                        ? "TIME TO COMPLETE"
                        : status === "CONFIRMED"
                          ? "ADMISSION STATUS"
                          : "HOLD STATUS"}
                    </span>
                    <strong>
                      {isHeld
                        ? formatRemainingTime(hold.expiresAt, now)
                        : status === "CONFIRMED"
                          ? "Seat confirmed"
                          : "Payment window closed"}
                    </strong>
                  </div>
                  <div className="application-actions">
                    {(isHeld || expired) && (
                      <>
                        <button
                          className="button button-dark"
                          disabled={confirming || cancelling || expired}
                          onClick={() => void handleConfirm(hold)}
                        >
                          {expired
                            ? "Hold expired"
                            : confirming
                              ? "Processing..."
                              : "Pay admission fee"}
                        </button>
                        {isHeld && (
                          <button
                            className="button button-light"
                            disabled={confirming || cancelling}
                            onClick={() => void handleCancel(hold)}
                          >
                            {cancelling ? "Cancelling..." : "Cancel"}
                          </button>
                        )}
                      </>
                    )}
                    {status === "CONFIRMED" && <span className="confirmed-note">Payment complete</span>}
                    {expired && <span className="expired-note">Payment unavailable</span>}
                  </div>
                </article>
              );
            })}
            {pastApplications.length > 0 && (
              <details className="past-applications">
                <summary>Show {pastApplications.length} past application{pastApplications.length === 1 ? "" : "s"}</summary>
                <div className="past-list">
                  {pastApplications.map((hold) => {
                    const status = currentHoldStatus(hold, now);
                    return (
                      <div className="past-row" key={hold.holdId}>
                        <span>{hold.className}</span>
                        <span className={`status-pill status-${status.toLowerCase()}`}>
                          <span className="status-dot" />{status}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </details>
            )}
          </div>
        )}
      </section>

      <section className="admin-section" id="school-admin">
        <div className="admin-intro">
          <p className="eyebrow">SCHOOL ADMIN</p>
          <h2>Set up a new class</h2>
          <p>Add a class or shift and its available seat capacity.</p>
        </div>
        <form className="admin-form" onSubmit={(event) => void handleCreateClass(event)}>
          <label>
            Class or shift name
            <input
              value={className}
              onChange={(event) => setClassName(event.target.value)}
              placeholder="e.g. Class 6 - Morning"
              required
            />
          </label>
          <label>
            Total seats
            <input
              type="number"
              min="1"
              step="1"
              value={totalSeats}
              onChange={(event) => setTotalSeats(event.target.value)}
              placeholder="e.g. 50"
              required
            />
          </label>
          <button
            className="button button-dark"
            type="submit"
            disabled={busyAction === "create-class"}
          >
            {busyAction === "create-class" ? "Creating..." : "Create class"}
            <span aria-hidden="true">↗</span>
          </button>
        </form>
      </section>

      <footer className="site-footer">
        <a className="brand" href="#">
          <span className="brand-mark" aria-hidden="true">SA</span>
          <span>School Admissions</span>
        </a>
        <span>YOUR FUTURE, ONE STEP AT A TIME.</span>
        <span>STUDENT ADMISSIONS · 2026</span>
      </footer>
    </main>
  );
}
