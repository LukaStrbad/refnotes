import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { GroupDto, UpdateGroupDto } from '../../model/user-group';
import { GroupCardComponent } from "./group-card/group-card.component";
import { CreateGroupModalComponent } from "../components/modals/create-group-modal/create-group-modal.component";
import { UserGroupService } from '../../services/user-group.service';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { GroupLinkCreatedComponent } from "../components/modals/group-link-created/group-link-created.component";
import { NotificationService } from '../../services/notification.service';
import { getTranslation } from '../../utils/translation-utils';
import { LoggerService } from '../../services/logger.service';
import { TestTagDirective } from '../../directives/test-tag.directive';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { EditGroupModalComponent } from "../components/modals/edit-group-modal/edit-group-modal.component";
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-groups',
  imports: [TranslatePipe, GroupCardComponent, CreateGroupModalComponent, GroupLinkCreatedComponent, TestTagDirective, EditGroupModalComponent],
  templateUrl: './groups.component.html',
  styleUrl: './groups.component.css'
})
export class GroupsComponent implements AfterViewInit {
  groups: GroupDto[] = [];

  @ViewChild('groupLinkCreatedModal')
  groupLinkCreatedModal!: GroupLinkCreatedComponent;

  constructor(
    private userGroupService: UserGroupService,
    private notificationService: NotificationService,
    private translateService: TranslateService,
    private logger: LoggerService,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
  ) { }

  ngAfterViewInit(): void {
    this.refreshGroups().then(() => {
      if (this.router.url.startsWith('/join-group')) {
        const params = this.route.snapshot.params;
        this.joinGroup(params).then();
      }
    });
  }

  async joinGroup(params: Params) {
    const groupId = params['id'];
    const accessCode = params['code'];

    const groupIdValid = !isNaN(Number(groupId));
    const accessCodeValid = typeof accessCode === 'string' && accessCode.length > 0;
    if (!groupIdValid || !accessCodeValid) {
      this.notificationService.error(
        await getTranslation(this.translateService, 'groups.invalid-join-group-link')
      );
      return;
    }

    // check if user is already in the group
    if (this.groups.some(group => group.id === Number(groupId))) {
      this.notificationService.info(
        await getTranslation(this.translateService, 'groups.user-already-in-group')
      );
      return;
    }

    this.logger.info('Join group with ID:', groupId, 'and access code:', accessCode);

    await this.notificationService.awaitAndNotifyError(
      this.userGroupService.addCurrentUserWithCode(groupId, accessCode),
      {
        default: await getTranslation(this.translateService, 'groups.join-group-error')
      },
      this.logger,
    );

    this.notificationService.success(
      await getTranslation(this.translateService, 'groups.join-group-success')
    );

    // Navigate to the groups page
    this.router.navigate(['/groups']).then();
  }

  async refreshGroups() {
    const observable = this.userGroupService.getUserGroupsCached();
    this.groups = await firstValueFrom(observable);
    this.groups = await lastValueFrom(observable);
  }

  async onGroupCreated(name: string) {
    const newGroup = await this.userGroupService.create(name);
    this.groups.push(newGroup);
  }

  async onGroupUpdated([groupId, updateDto]: [number, UpdateGroupDto]) {
    await this.notificationService.awaitAndNotifyError(this.userGroupService.update(groupId, updateDto),
      {
        default: await getTranslation(this.translateService, 'groups.update-group-error')
      },
      this.logger
    );

    this.notificationService.success(
      await getTranslation(this.translateService, 'groups.update-group-success')
    );

    // Update the group locally to avoid refetching
    const groupIndex = this.groups.findIndex(group => group.id === groupId);
    if (groupIndex !== -1) {
      this.groups[groupIndex].name = updateDto.name;
    }
  }

  async createInviteLink(group: GroupDto) {
    const accessCode = await this.notificationService.awaitAndNotifyError(
      this.userGroupService.generateAccessCode(group.id),
      {
        403: await getTranslation(this.translateService, 'error.access-code-not-allowed'),
        default: await getTranslation(this.translateService, 'error.error-creating-access-code')
      },
      this.logger,
    );

    const accessCodeEncoded = encodeURIComponent(accessCode);

    const domain = window.location.origin;
    const link = `${domain}/join-group/${group.id}/${accessCodeEncoded}`;
    this.logger.info('Generated access link:', link);

    this.groupLinkCreatedModal.show(link);
  }

  async onLeaveGroup(group: GroupDto) {
    const user = this.authService.user();
    if (!user) {
      this.logger.error('User not found when trying to leave group');
      return;
    }

    await this.notificationService.awaitAndNotifyError(
      this.userGroupService.leaveGroup(group.id),
      {
        default: await getTranslation(this.translateService, 'groups.leave-group-error')
      },
      this.logger,
    );

    this.groups = this.groups.filter(g => g.id !== group.id);
    this.notificationService.success(
      await getTranslation(this.translateService, 'groups.leave-group-success')
    );
    this.logger.info('Left group:', group.id);
  }
}
