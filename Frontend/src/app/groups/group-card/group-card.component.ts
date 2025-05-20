import { AfterViewInit, Component, EventEmitter, Input, Output } from '@angular/core';
import { GroupDto, GroupUserDto, UserGroupRole } from '../../../model/user-group';
import { UserGroupService } from '../../../services/user-group.service';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { LoggerService } from '../../../services/logger.service';
import { TestTagDirective } from '../../../directives/test-tag.directive';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-group-card',
  imports: [TranslatePipe, TranslateDirective, TestTagDirective, RouterLink],
  templateUrl: './group-card.component.html',
  styleUrl: './group-card.component.css'
})
export class GroupCardComponent implements AfterViewInit {
  @Input({ required: true }) group!: GroupDto;

  @Output()
  invite = new EventEmitter<GroupDto>();

  @Output()
  edit = new EventEmitter<GroupDto>();

  members: GroupUserDto[] | null = null;
  canInviteMembers = false;
  canEditGroup = false;

  constructor(
    private userGroupService: UserGroupService,
    private log: LoggerService,
  ) { }

  ngAfterViewInit(): void {
    this.fetchMembers().then();
    this.canInviteMembers = this.canInvite();
    this.canEditGroup = this.canEdit() || true;
  }

  async fetchMembers() {
    this.log.info('Fetching members for group', this.group.id);
    const observable = this.userGroupService.getGroupMembersCached(this.group.id);
    this.members = await firstValueFrom(observable);
    this.members = await lastValueFrom(observable);
  }

  canInvite(): boolean {
    return this.group.role === UserGroupRole.Owner || this.group.role === UserGroupRole.Admin;
  }

  canEdit(): boolean {
    return this.group.role === UserGroupRole.Owner || this.group.role === UserGroupRole.Admin;
  }

  onInvite() {
    this.log.info('Inviting members to group', this.group.id);
    this.invite.emit(this.group);
  }

  onEdit() {
    this.log.info('Editing group', this.group.id);
    this.edit.emit(this.group);
  }
}
