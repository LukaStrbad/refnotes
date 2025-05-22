import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { UserGroupService } from '../../../services/user-group.service';
import { AssignRoleDto, GroupUserDto, UserGroupRole } from '../../../model/user-group';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { AskModalService } from '../../../services/ask-modal.service';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LoggerService } from '../../../services/logger.service';
import { NotificationService } from '../../../services/notification.service';
import { getTranslation } from '../../../utils/translation-utils';
import { TestTagDirective } from '../../../directives/test-tag.directive';
import { AuthService } from '../../../services/auth.service';
import { GroupBadgeComponent } from "../group-badge/group-badge.component";

@Component({
  selector: 'app-group-members-list',
  imports: [TranslatePipe, TranslateDirective, TestTagDirective, RouterLink, GroupBadgeComponent],
  templateUrl: './group-members-list.component.html',
  styleUrl: './group-members-list.component.css'
})
export class GroupMembersListComponent implements OnInit {
  readonly UserGroupRole = UserGroupRole; // Make enum available in template
  groupId: number;
  members: GroupUserDto[] | null = null;
  selectedUser: GroupUserDto | null = null;
  currentUser: GroupUserDto | null = null;

  // Available roles for role selector, excluding Owner since it can't be assigned
  readonly availableRoles = [UserGroupRole.Admin, UserGroupRole.Member];

  @ViewChild('roleModal') roleModal!: ElementRef<HTMLDialogElement>;
  @ViewChild('roleSelect') roleSelect!: ElementRef<HTMLSelectElement>;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userGroupService: UserGroupService,
    private askModal: AskModalService,
    private translateService: TranslateService,
    private logger: LoggerService,
    private notificationService: NotificationService,
    private auth: AuthService,
  ) {
    this.groupId = Number(this.route.snapshot.paramMap.get('id'));
  }

  ngOnInit(): void {
    this.fetchMembers();
  }

  async fetchMembers() {
    this.logger.info('Fetching members for group', this.groupId);
    const membersObservable = this.userGroupService.getGroupMembersCached(this.groupId);
    this.members = await firstValueFrom(membersObservable);
    this.members = await lastValueFrom(membersObservable);

    if (!this.members) {
      return;
    }

    const user = this.auth.user;
    if (!user) {
      return;
    }

    this.currentUser = this.members.find(m => m.id === user.id) ?? null;
  }

  get canManageMembers(): boolean {
    return this.currentUser?.role === UserGroupRole.Owner || this.currentUser?.role === UserGroupRole.Admin;
  }

  openRoleModal(member: GroupUserDto) {
    this.selectedUser = member;
    this.roleModal.nativeElement.showModal();
  }

  canManageRole(member: GroupUserDto): boolean {
    if (!this.currentUser || !this.canManageMembers) return false;
    if (member.role === UserGroupRole.Owner) return false; // Can't manage owner's role

    const currentUserRole = this.currentUser.role;
    const targetUserRole = member.role;

    // Users can only manage roles of users with lower privileges
    if (currentUserRole === UserGroupRole.Owner) {
      return true; // Owner can manage all except other owners
    }

    if (currentUserRole === UserGroupRole.Admin) {
      return targetUserRole === UserGroupRole.Member; // Admin can only manage members
    }

    return false;
  }

  canRemoveUser(member: GroupUserDto): boolean {
    if (!this.currentUser) return false;
    if (member.role === UserGroupRole.Owner) return false; // Can't remove owner
    if (member.id === this.currentUser.id) return true; // Can always remove self
    if (!this.canManageMembers) return false;

    const currentUserRole = this.currentUser.role;
    const targetUserRole = member.role;

    // Similar logic to role management - can only remove users with lower privileges
    if (currentUserRole === UserGroupRole.Owner) {
      return true; // Owner can remove anyone except other owners
    }

    if (currentUserRole === UserGroupRole.Admin) {
      return targetUserRole === UserGroupRole.Member; // Admin can only remove members
    }

    return false;
  }

  async updateRole() {
    if (!this.selectedUser || !this.roleSelect) return;

    const newRole = Number(this.roleSelect.nativeElement.value) as UserGroupRole;
    if (newRole === this.selectedUser.role) {
      return; // No change needed
    }

    try {
      this.logger.info('Updating role for user', this.selectedUser.id, 'to', UserGroupRole[newRole]);
      const assignRole: AssignRoleDto = {
        userId: this.selectedUser.id,
        role: newRole
      };

      await this.userGroupService.assignRole(this.groupId, assignRole);

      // Update locally
      if (this.members) {
        const memberIndex = this.members.findIndex(m => m.id === this.selectedUser?.id);
        if (memberIndex !== -1) {
          this.members[memberIndex] = {
            ...this.members[memberIndex],
            role: newRole
          };
        }
      }

      this.notificationService.success(
        await getTranslation(this.translateService, 'group.members.role-updated')
      );
    } catch (error: unknown) {
      this.logger.error('Error updating role:', error);
      this.notificationService.error(
        await getTranslation(this.translateService, 'group.members.error-updating-role')
      );
    }

    this.selectedUser = null;
    this.roleModal.nativeElement.close();
  }

  async removeUser(member: GroupUserDto) {
    const confirmed = await this.askModal.confirm(
      await getTranslation(this.translateService, 'group.members.confirm-remove'),
      await getTranslation(this.translateService, 'group.members.confirm-remove-description', {
        username: member.username
      }),
      { translate: false }
    );

    if (!confirmed) return;

    try {
      this.logger.info('Removing user', member.id, 'from group', this.groupId);
      await this.userGroupService.removeUser(this.groupId, member.id);

      // Update locally
      if (this.members) {
        const memberIndex = this.members.findIndex(m => m.id === member.id);
        if (memberIndex !== -1) {
          this.members.splice(memberIndex, 1);
        }
      }

      this.notificationService.success(
        await getTranslation(this.translateService, 'group.members.user-removed')
      );

      // If the user removed themselves, redirect to groups page
      if (member.id === this.currentUser?.id) {
        this.router.navigate(['/groups']);
      }
    } catch (error: unknown) {
      this.logger.error('Error removing user:', error);
      this.notificationService.error(
        await getTranslation(this.translateService, 'group.members.error-removing-user')
      );
    }
  }
}
