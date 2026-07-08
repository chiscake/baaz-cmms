/** @param {unknown} error */
export function formatError(error) {
  if (!error) {
    return "Неизвестная ошибка";
  }

  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === "object") {
    const record = /** @type {{ message?: string, details?: string, hint?: string, code?: string }} */ (
      error
    );
    const parts = [record.message, record.details, record.hint].filter(Boolean);
    if (parts.length > 0) {
      return parts.join(" — ");
    }
    try {
      return JSON.stringify(error);
    } catch {
      return String(error);
    }
  }

  return String(error);
}
