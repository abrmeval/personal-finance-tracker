export const AppStatusCode = {
  /** Custom code for network errors (e.g., CORS issues, no response) */
  NetworkError: 0,
  OK: 200,
  Created: 201,
  NoContent: 204,
  BadRequest: 400,
  Unauthorized: 401,
  Forbidden: 403,
  NotFound: 404,
  Conflict: 409,
  /** Custom code for unprocessable content (e.g., validation errors, ) */
  UnprocessableContent: 422,
  InternalServerError: 500,
  /** Custom code for parsing errors (e.g., invalid JSON) */
  ParseError: 600,
  /** Custom code for client errors */
  ClientError: 601,
};

/**
 * Represents an error response from the API, including details about the error, context, and any model validation errors.
 */
export class ApiError extends Error {
  title: string;
  context?: string;
  instance?: string;
  modelErrors?: Record<string, string[]> | null;
  status?: number;

  /**
   * Creates a new instance of the ApiError class.
   * @param title The title of the error
   * @param detail Error details
   * @param context  Error context
   * @param instance Error URI that identifies the specific occurrence
   * @param status The HTTP status code
   * @param modelErrors Model validation errors
   */
  constructor(
    title: string,
    detail: string,
    context?: string,
    instance?: string,
    status?: number,
    modelErrors?: Record<string, string[]> | null,
  ) {
    super(detail);
    this.name = "ApiError";
    this.context = context;
    this.instance = instance;
    this.modelErrors = modelErrors;
    this.title = title;
    this.status = status;
  }
}

export interface ApiResponse<T> {
  isOk: boolean;
  data?: T | null;
  error?: ApiError | null;
  statusCode?: number;
  codeText?: string;
}
