import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserGroupService } from './user-group.service';
import { environment } from '../environments/environment';
import { AssignRoleDto, GroupDto, GroupUserDto, UpdateGroupDto, UserGroupRole } from '../model/user-group';
import { provideHttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { AuthService } from './auth.service';
import { signal } from '@angular/core';

describe('UserGroupService', () => {
  let service: UserGroupService;
  let httpMock: HttpTestingController;
  let authService: jasmine.SpyObj<AuthService>;
  const apiUrl = environment.apiUrl + '/UserGroup';

  beforeEach(() => {
    authService = jasmine.createSpyObj('AuthService', [], { user: signal({ id: 1 }) });

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        UserGroupService,
        { provide: AuthService, useValue: authService }
      ]
    });
    service = TestBed.inject(UserGroupService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should create a group', async () => {
    const groupName = 'Test Group';
    const expectedGroup: GroupDto = { id: 1, name: groupName, role: UserGroupRole.Owner };

    const promise = service.create(groupName);

    const req = httpMock.expectOne(`${apiUrl}/create?name=${groupName}`);
    expect(req.request.method).toBe('POST');
    req.flush(expectedGroup);

    const group = await promise;
    expect(group).toEqual(expectedGroup);
  });

  it('should update a group', async () => {
    const groupId = 123;
    const updateGroup: UpdateGroupDto = {
      name: 'Updated Group'
    };

    const promise = service.update(groupId, updateGroup);

    const req = httpMock.expectOne(`${apiUrl}/${groupId}/update`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(updateGroup);
    req.flush(null);

    await promise;
  });

  it('should get user groups', async () => {
    const mockGroups: GroupDto[] = [
      { id: 1, name: 'Group 1', role: UserGroupRole.Owner },
      { id: 2, name: 'Group 2', role: UserGroupRole.Member }
    ];

    const promise = lastValueFrom(service.getUserGroupsCached());

    const req = httpMock.expectOne(`${apiUrl}/getUserGroups`);
    expect(req.request.method).toBe('GET');
    req.flush(mockGroups);

    const groups = await promise;
    expect(groups).toEqual(mockGroups);
  });

  it('should get group members', async () => {
    const groupId = 123;
    const mockMembers: GroupUserDto[] = [
      { id: 1, username: 'user1', role: UserGroupRole.Member },
      { id: 2, username: 'user2', role: UserGroupRole.Admin }
    ];

    const promise = lastValueFrom(service.getGroupMembersCached(groupId));

    const req = httpMock.expectOne(`${apiUrl}/${groupId}/members`);
    expect(req.request.method).toBe('GET');
    req.flush(mockMembers);

    const members = await promise;
    expect(members).toEqual(mockMembers);
  });

  it('should assign role', async () => {
    const groupId = 123;
    const assignRole: AssignRoleDto = {
      userId: 456,
      role: UserGroupRole.Admin
    };

    const promise = service.assignRole(groupId, assignRole);

    const req = httpMock.expectOne(`${apiUrl}/${groupId}/assignRole`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(assignRole);
    req.flush(null);

    await promise;
  });

  it('should remove user', async () => {
    const groupId = 123;
    const userId = 456;

    const promise = service.removeUser(groupId, userId);

    const req = httpMock.expectOne(`${apiUrl}/${groupId}/removeUser?userId=${userId}`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    await promise;
  });

  it('should generate access code', async () => {
    const groupId = 123;
    const expiryTime = new Date('2025-12-31');
    const mockCode = 'ABC123';

    const promise = service.generateAccessCode(groupId, expiryTime);

    const req = httpMock.expectOne(`${apiUrl}/${groupId}/generateAccessCode`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(expiryTime);
    req.flush(mockCode);

    const code = await promise;
    expect(code).toBe(mockCode);
  });

  it('should add current user with code', async () => {
    const groupId = 123;
    const accessCode = 'ABC123';

    const promise = service.addCurrentUserWithCode(groupId, accessCode);

    const req = httpMock.expectOne(`${apiUrl}/${groupId}/addCurrentUserWithCode`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBe(JSON.stringify(accessCode));
    req.flush(null);

    await promise;
  });
});
