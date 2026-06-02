export interface ClientLogEntry {
  message: string;
  statusCode?: number;
  details?: string;
  path?: string;
  context?: string;
  shouldLogEvent?: boolean;
}

/**
 * ClientLogger is a utility class for logging messages to the console with different severity levels (info, warning, error).
 * It allows you to include additional context such as status codes, server messages, and paths where events occurred.
 */
export class ClientLogger {
  static LogInfo({
    message,
    statusCode,
    details: originalMessage,
    path,
    context,
    shouldLogEvent = import.meta.env.VITE_ENVIRONMENT !== "production",
  }: ClientLogEntry) {
    if (!shouldLogEvent) return;

    console.info(
      `[INFO] ${message}`,
      context ? `Context: ${context}` : "",
      statusCode ? `Status Code: ${statusCode}` : "",
      originalMessage ? `Original Message: ${originalMessage}` : "",
      path ? `Path: ${path}` : "",
    );
  }

 static LogWarning({
    message,
    statusCode,
    details: originalMessage,
    path,
    context,
    shouldLogEvent = import.meta.env.VITE_ENVIRONMENT !== "production",
  }: ClientLogEntry) {
    if (!shouldLogEvent) return;

    console.warn(
      `[WARNING] ${message}`,
      context ? `Context: ${context}` : "",
      statusCode ? `Status Code: ${statusCode}` : "",
      originalMessage ? `Original Message: ${originalMessage}` : "",
      path ? `Path: ${path}` : "",
    );

  }
  
 static LogError({
    message,
    statusCode,
    details: originalMessage,
    path,
    context,
    shouldLogEvent = import.meta.env.VITE_ENVIRONMENT !== "production",
  }: ClientLogEntry) {
    if (!shouldLogEvent) return;

    console.error(
      `[ERROR] ${message}`,
      context ? `Context: ${context}` : "",
      statusCode ? `Status Code: ${statusCode}` : "",
      originalMessage ? `Original Message: ${originalMessage}` : "",
      path ? `Path: ${path}` : "",
    );
  }
}
