import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error('Required root element "#root" was not found.');
}

createRoot(rootElement).render(<StrictMode />);
