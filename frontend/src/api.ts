export const API_BASE_URL = "http://localhost:8080";

export interface AdmissionClassSummary {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface StudentHold {
  holdId: string;
  classId: string;
  className: string;
  status: string;
  expiresAt: string;
}

export interface CreatedHold {
  holdId: string;
  classId: string;
  studentId: string;
  status: string;
  expiresAt: string;
}

interface ApiProblem {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status?: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(
  path: string,
  studentId?: string,
  init?: RequestInit,
): Promise<T> {
  const headers = new Headers(init?.headers);
  if (studentId) {
    headers.set("X-Student-Id", studentId);
  }
  if (init?.body) {
    headers.set("Content-Type", "application/json");
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });
  } catch {
    throw new ApiError(
      "The admission service could not be reached. Check that the backend is running at http://localhost:8080.",
    );
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as ApiProblem | null;
    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat().join(" ")
      : undefined;
    const message = validationMessage || problem?.detail || problem?.title;

    throw new ApiError(
      message || `The request could not be completed (${response.status}).`,
      response.status,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return await response.json() as T;
}

export const admissionApi = {
  getClasses: () => request<AdmissionClassSummary[]>("/classes"),

  createClass: (name: string, totalSeats: number) =>
    request<AdmissionClassSummary>("/classes", undefined, {
      method: "POST",
      body: JSON.stringify({ name, totalSeats }),
    }),

  getStudentHolds: (studentId: string) =>
    request<StudentHold[]>("/me/holds", studentId),

  createHold: (classId: string, studentId: string) =>
    request<CreatedHold>(`/classes/${encodeURIComponent(classId)}/holds`, studentId, {
      method: "POST",
    }),

  confirmHold: (holdId: string, studentId: string) =>
    request<{ holdId: string; status: string }>(
      `/holds/${encodeURIComponent(holdId)}/confirm`,
      studentId,
      { method: "POST" },
    ),

  cancelHold: (holdId: string, studentId: string) =>
    request<void>(`/holds/${encodeURIComponent(holdId)}`, studentId, {
      method: "DELETE",
    }),
};
