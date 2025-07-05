import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GroupMembersListComponent } from './group-members-list.component';
import { ActivatedRoute, Router } from '@angular/router';
import { UserGroupService } from '../../../services/user-group.service';
import { AskModalService } from '../../../services/ask-modal.service';
import { TranslateModule } from '@ngx-translate/core';
import { LoggerService } from '../../../services/logger.service';
import { NotificationService } from '../../../services/notification.service';
import { AuthService } from '../../../services/auth.service';
import { GroupUserDto, UserGroupRole } from '../../../model/user-group';
import { ElementRef } from '@angular/core';
import { of } from 'rxjs';
import { User } from '../../../model/user';

describe('GroupMembersListComponent', () => {
  let component: GroupMembersListComponent;
  let fixture: ComponentFixture<GroupMembersListComponent>;
  let userGroupService: jasmine.SpyObj<UserGroupService>;
  let askModal: jasmine.SpyObj<AskModalService>;
  let logger: jasmine.SpyObj<LoggerService>;
  let notificationService: jasmine.SpyObj<NotificationService>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  const mockMembers = [
    { id: 1, username: 'owner', role: UserGroupRole.Owner },
    { id: 2, username: 'admin', role: UserGroupRole.Admin },
    { id: 3, username: 'member', role: UserGroupRole.Member },
  ];

  const mockUser: User = {
    id: 1,
    username: 'owner',
    name: 'Owner User',
    email: 'owner@test.com'
  };

  function setupMockMembers(members: GroupUserDto[]) {
    // Mock the members in order to avoid using the same mockMembers reference in all tests
    // which leads to race conditions and unexpected behavior
    const membersCopy = [...members];
    userGroupService.getGroupMembersCached.and.returnValue(of(membersCopy));
  }

  beforeEach(async () => {
    userGroupService = jasmine.createSpyObj('UserGroupService', ['getGroupMembersCached', 'assignRole', 'removeUser']);
    askModal = jasmine.createSpyObj('AskModalService', ['confirm']);
    logger = jasmine.createSpyObj('LoggerService', ['info', 'error']);
    notificationService = jasmine.createSpyObj('NotificationService', ['success', 'error']);
    authService = jasmine.createSpyObj('AuthService', [], { user: mockUser });
    router = jasmine.createSpyObj('Router', ['navigate']);

    askModal.confirm.and.resolveTo(true);

    await TestBed.configureTestingModule({
      imports: [
        GroupMembersListComponent,
        TranslateModule.forRoot()
      ],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '1' } } } },
        { provide: Router, useValue: router },
        { provide: UserGroupService, useValue: userGroupService },
        { provide: AskModalService, useValue: askModal },
        { provide: LoggerService, useValue: logger },
        { provide: NotificationService, useValue: notificationService },
        { provide: AuthService, useValue: authService },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupMembersListComponent);
    component = fixture.componentInstance;

    // Mock the role modal element
    component.roleModal = new ElementRef<HTMLDialogElement>(document.createElement('dialog'));
    component.roleSelect = new ElementRef<HTMLSelectElement>(document.createElement('select'));

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load members on init', async () => {
    setupMockMembers(mockMembers);
    component.ngOnInit();
    await fixture.whenStable();

    expect(component.members).toEqual(mockMembers);
    expect(userGroupService.getGroupMembersCached).toHaveBeenCalledWith(1);
  });

  it('should set currentUser from members list', async () => {
    setupMockMembers(mockMembers);
    await component.fetchMembers();
    fixture.detectChanges();

    expect(component.currentUser).toEqual(mockMembers[0]); // ID 1 is the owner
  });

  describe('canManageMembers', () => {
    it('should return true for owner', async () => {
      setupMockMembers(mockMembers);
      await component.fetchMembers();

      expect(component.canManageMembers).toBeTrue();
    });

    it('should return true for admin', async () => {
      setupMockMembers(mockMembers);
      Object.defineProperty(authService, 'user', {
        get: () => ({ ...mockUser, id: 2, username: 'admin' })
      });
      await component.fetchMembers();

      expect(component.canManageMembers).toBeTrue();
    });

    it('should return false for regular member', async () => {
      setupMockMembers(mockMembers);
      Object.defineProperty(authService, 'user', {
        get: () => ({ ...mockUser, id: 3, username: 'member' })
      });
      await component.fetchMembers();

      expect(component.canManageMembers).toBeFalse();
    });
  });

  describe('role management', () => {
    it('should correctly determine if user can manage roles', async () => {
      setupMockMembers(mockMembers);
      await component.fetchMembers();
      fixture.detectChanges();

      const ownerMember = mockMembers[0];
      const adminMember = mockMembers[1];
      const regularMember = mockMembers[2];

      // Owner can manage admin and regular member roles
      component.currentUser = ownerMember;
      expect(component.canManageRole(adminMember)).toBeTrue();
      expect(component.canManageRole(regularMember)).toBeTrue();
      expect(component.canManageRole(ownerMember)).toBeFalse(); // Can't manage other owners

      // Admin can only manage regular members
      component.currentUser = adminMember;
      expect(component.canManageRole(ownerMember)).toBeFalse();
      expect(component.canManageRole(adminMember)).toBeFalse();
      expect(component.canManageRole(regularMember)).toBeTrue();

      // Regular member can't manage any roles
      component.currentUser = regularMember;
      expect(component.canManageRole(ownerMember)).toBeFalse();
      expect(component.canManageRole(adminMember)).toBeFalse();
      expect(component.canManageRole(regularMember)).toBeFalse();
    });

    it('should open role modal', async () => {
      setupMockMembers(mockMembers);
      await component.fetchMembers();
      fixture.detectChanges();

      const member = mockMembers[2];
      const showModalSpy = spyOn(component.roleModal.nativeElement, 'showModal');

      component.openRoleModal(member);

      expect(component.selectedUser).toBe(member);
      expect(showModalSpy).toHaveBeenCalled();
    });

    it('should update member role', async () => {
      setupMockMembers(mockMembers);
      await component.fetchMembers();
      fixture.detectChanges();

      const member = mockMembers[2];
      const newRole = UserGroupRole.Admin;

      component.selectedUser = member;
      component.roleSelect.nativeElement.value = newRole.toString();
      userGroupService.assignRole.and.resolveTo();

      await component.updateRole();

      expect(userGroupService.assignRole).toHaveBeenCalledWith(1, {
        userId: member.id,
        role: newRole
      });
      expect(notificationService.success).toHaveBeenCalled();
      expect(component.selectedUser).toBeNull();
    });

    it('should handle role update error', async () => {
      setupMockMembers(mockMembers);
      await component.fetchMembers();
      fixture.detectChanges();

      const member = mockMembers[2];
      const newRole = UserGroupRole.Admin;

      component.selectedUser = member;
      component.roleSelect.nativeElement.value = newRole.toString();
      userGroupService.assignRole.and.rejectWith(new Error('Test error'));

      await component.updateRole();

      expect(logger.error).toHaveBeenCalled();
      expect(notificationService.error).toHaveBeenCalled();
      expect(component.selectedUser).toBeNull();
    });
  });

  describe('user removal', () => {
    it('should correctly determine if user can remove members', async () => {
      const otherAdmin = { id: 4, username: 'otherAdmin', role: UserGroupRole.Admin };
      const otherMember = { id: 5, username: 'otherMember', role: UserGroupRole.Member };
      const allMembers = [...mockMembers, otherAdmin, otherMember];

      setupMockMembers(allMembers);
      await component.fetchMembers();

      const ownerMember = mockMembers[0];
      const adminMember = mockMembers[1];
      const regularMember = mockMembers[2];

      // Owner can remove anyone except other owners
      component.currentUser = ownerMember;
      expect(component.canRemoveUser(ownerMember)).toBeFalse();
      expect(component.canRemoveUser(otherAdmin)).toBeTrue();
      expect(component.canRemoveUser(otherMember)).toBeTrue();

      // Admin can only remove regular members
      component.currentUser = adminMember;
      expect(component.canRemoveUser(ownerMember)).toBeFalse();
      expect(component.canRemoveUser(otherAdmin)).toBeFalse();
      expect(component.canRemoveUser(otherMember)).toBeTrue();

      // Member can only remove themselves
      component.currentUser = regularMember;
      expect(component.canRemoveUser(ownerMember)).toBeFalse();
      expect(component.canRemoveUser(adminMember)).toBeFalse();
      expect(component.canRemoveUser(regularMember)).toBeTrue(); // Can remove self
    });

    it('should remove user after confirmation', async () => {
      const member = mockMembers[2];
      userGroupService.removeUser.and.resolveTo();
      askModal.confirm.and.resolveTo(true);
      component.members = [...mockMembers];

      await component.removeUser(member);

      expect(askModal.confirm).toHaveBeenCalled();
      expect(userGroupService.removeUser).toHaveBeenCalledWith(1, member.id);
      expect(notificationService.success).toHaveBeenCalled();
      expect(component.members).not.toContain(member);
    });

    it('should not remove user if confirmation is cancelled', async () => {
      const member = mockMembers[2];
      askModal.confirm.and.resolveTo(false);
      component.members = [...mockMembers];

      await component.removeUser(member);

      expect(askModal.confirm).toHaveBeenCalled();
      expect(userGroupService.removeUser).not.toHaveBeenCalled();
      expect(component.members).toContain(member);
    });

    it('should handle user removal error', async () => {
      const member = mockMembers[2];
      userGroupService.removeUser.and.rejectWith(new Error('Test error'));
      askModal.confirm.and.resolveTo(true);
      component.members = [...mockMembers];

      await component.removeUser(member);

      expect(logger.error).toHaveBeenCalled();
      expect(notificationService.error).toHaveBeenCalled();
      expect(component.members).toContain(member);
    });

    it('should navigate to groups page when removing self', async () => {
      const member = mockMembers[0]; // Current user (owner)
      userGroupService.removeUser.and.resolveTo();
      askModal.confirm.and.resolveTo(true);
      setupMockMembers(mockMembers);
      await component.fetchMembers();
      fixture.detectChanges();

      await component.removeUser(member);

      expect(router.navigate).toHaveBeenCalledWith(['/groups']);
    });
  });
});
