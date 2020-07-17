export interface Pagination {
    currentPage: number;
    itemsPerPage: number;
    totalItems: number;
    totalPages: number;
}
// store result in 2 part: user, pagination information
export class PaginatedResult<T> {
    result: T;
    pagination: Pagination;
}
