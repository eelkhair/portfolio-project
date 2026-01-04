export interface ApiError {
  message?: string;
  errors?: Record<string, string[]>;
}

export interface ApiResponse<T> {
  exceptions?: ApiError;
  data?: T;
  success: boolean;
  statusCode: number; // could replace with an enum if you want
}
