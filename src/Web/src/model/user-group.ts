export interface UpdateGroupDto {
    name: string;
}

export enum UserGroupRole {
  Owner = 0,
  Admin = 1,
  Member = 2,
}

export interface GroupDto {
    id: number;
    name: string;
    role: UserGroupRole;
}

export interface GroupUserDto {
    id: number;
    username: string;
    role: UserGroupRole;
}

export interface AssignRoleDto {
    userId: number;
    role: UserGroupRole;
}

export interface GroupDetails {
    id: number;
    name: string;
}
