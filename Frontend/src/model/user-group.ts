export interface UpdateGroupDto {
    name: string;
}

export interface GroupDto {
    id: number;
    name: string;
    role: string;
}

export interface GroupUserDto {
    id: number;
    username: string;
    role: string;
}

export interface AssignRoleDto {
    userId: number;
    role: string;
}
