export interface UserResponse {
    id: number;
    username: string;
    name: string;
    email: string,
    roles: string[];
    emailConfirmed: boolean;
}
