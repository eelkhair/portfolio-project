export interface ApiResponse<T> {
  data?: T;
  success: boolean;
  statusCode: number;
  exceptions?: { message?: string; errors?: Record<string, string[]> };
}

export interface PaginatedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
