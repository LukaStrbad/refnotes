import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { AssignRoleDto, GroupDto, GroupUserDto, UpdateGroupDto } from '../model/user-group';
import { firstValueFrom } from 'rxjs';

const apiUrl = environment.apiUrl + '/UserGroup';

@Injectable({
  providedIn: 'root'
})
export class UserGroupService {
  constructor(private http: HttpClient) { }

  async create(name?: string): Promise<number> {
    return firstValueFrom(this.http.post<number>(`${apiUrl}/create?name=${name}`, null));
  }

  async update(groupId: number, updateGroup: UpdateGroupDto): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${apiUrl}/${groupId}/update`, updateGroup));
  }

  async getUserGroups(): Promise<GroupDto[]> {
    return firstValueFrom(this.http.get<GroupDto[]>(`${apiUrl}/getUserGroups`));
  }

  async getGroupMembers(groupId: number): Promise<GroupUserDto[]> {
    return firstValueFrom(this.http.get<GroupUserDto[]>(`${apiUrl}/${groupId}/members`));
  }

  async assignRole(groupId: number, assignRole: AssignRoleDto): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${apiUrl}/${groupId}/assignRole`, assignRole));
  }

  async removeUser(groupId: number, userId: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${apiUrl}/${groupId}/removeUser`, { params: { userId } }));
  }

  async generateAccessCode(groupId: number, expiryTime?: Date): Promise<string> {
    return firstValueFrom(this.http.post<string>(`${apiUrl}/${groupId}/generateAccessCode`, expiryTime));
  }

  async addCurrentUserWithCode(groupId: number, accessCode: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${apiUrl}/${groupId}/addCurrentUserWithCode`, accessCode));
  }
}
