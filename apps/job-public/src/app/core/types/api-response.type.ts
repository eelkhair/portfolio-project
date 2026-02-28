export interface ApiResponse<T> {
  data?: T;
  success: boolean;
  statusCode: number;
  exceptions?: { message?: string; errors?: Record<string, string[]> };
}
