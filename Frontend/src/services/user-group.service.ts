import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { AssignRoleDto, GroupDto, GroupUserDto, UpdateGroupDto } from '../model/user-group';
import { firstValueFrom, merge, Observable, of, tap } from 'rxjs';
import { LRUCache } from 'lru-cache';
import { AuthService } from './auth.service';

const apiUrl = environment.apiUrl + '/UserGroup';

@Injectable({
  providedIn: 'root'
})
export class UserGroupService {
  private readonly membersCache = new LRUCache<number, GroupDto[]>({
    max: 100,
  });
  private readonly groupMembersCache = new LRUCache<number, GroupUserDto[]>({
    max: 100,
  });

  constructor(
    private http: HttpClient,
    private auth: AuthService
  ) { }

  async create(name?: string): Promise<GroupDto> {
    return firstValueFrom(this.http.post<GroupDto>(`${apiUrl}/create?name=${name}`, null));
  }

  async update(groupId: number, updateGroup: UpdateGroupDto): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${apiUrl}/${groupId}/update`, updateGroup));
  }

  getUserGroupsCached(): Observable<GroupDto[]> {
    const userId = this.auth.user?.id ?? -1;
    const cached = this.membersCache.get(userId);

    const network = this.http.get<GroupDto[]>(`${apiUrl}/getUserGroups`).pipe(
      tap((groups) => {
        this.membersCache.set(userId, groups);
      })
    );

    return cached ? merge(of(cached), network) : network;
  }

  getGroupMembersCached(groupId: number): Observable<GroupUserDto[]> {
    const cached = this.groupMembersCache.get(groupId);
    const network = this.http.get<GroupUserDto[]>(`${apiUrl}/${groupId}/members`).pipe(
      tap((members) => {
        this.groupMembersCache.set(groupId, members);
      })
    );

    return cached ? merge(of(cached), network) : network;
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
