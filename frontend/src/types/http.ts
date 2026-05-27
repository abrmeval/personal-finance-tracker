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

export class ApiError extends Error {
  context?: string;
  path?: string;
  validationErrors?: Record<string, string[]> | null;
  statusCode?: number;

  constructor(
    message: string,
    context?: string,
    path?: string,
    statusCode?: number,
    validationErrors?: Record<string, string[]> | null,
  ) {
    super(message);
    this.name = "ApiError";
    this.context = context;
    this.path = path;
    this.validationErrors = validationErrors;
    this.statusCode = statusCode;
  }
}

export interface ApiResponse<T> {
  isOk: boolean;
  data?: T | null;
  error?: ApiError | null;
  statusCode?: number;
  codeText?: string;
}
