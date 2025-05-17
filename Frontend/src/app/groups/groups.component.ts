import { Component, ViewChild } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { GroupDto } from '../../model/user-group';
import { GroupCardComponent } from "./group-card/group-card.component";
import { CreateGroupModalComponent } from "../components/modals/create-group-modal/create-group-modal.component";
import { UserGroupService } from '../../services/user-group.service';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { GroupLinkCreatedComponent } from "../components/modals/group-link-created/group-link-created.component";
import { NotificationService } from '../../services/notification.service';
import { getTranslation } from '../../utils/translation-utils';
import { LoggerService } from '../../services/logger.service';

@Component({
  selector: 'app-groups',
  imports: [TranslatePipe, GroupCardComponent, CreateGroupModalComponent, GroupLinkCreatedComponent],
  templateUrl: './groups.component.html',
  styleUrl: './groups.component.css'
})
export class GroupsComponent {
  groups: GroupDto[] = [];

  @ViewChild('groupLinkCreatedModal')
  groupLinkCreatedModal!: GroupLinkCreatedComponent;

  constructor(
    private userGroupService: UserGroupService,
    private notificationService: NotificationService,
    private translateService: TranslateService,
    private logger: LoggerService,
  ) {
    this.refreshGroups().then();
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
}
