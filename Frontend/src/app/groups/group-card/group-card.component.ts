import { AfterViewInit, Component, Input } from '@angular/core';
import { GroupDto, GroupUserDto } from '../../../model/user-group';
import { UserGroupService } from '../../../services/user-group.service';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { LoggerService } from '../../../services/logger.service';

@Component({
  selector: 'app-group-card',
  imports: [TranslatePipe, TranslateDirective],
  templateUrl: './group-card.component.html',
  styleUrl: './group-card.component.css'
})
export class GroupCardComponent implements AfterViewInit {
  @Input({ required: true }) group!: GroupDto;

  members: GroupUserDto[] | null = null;

  constructor(
    private userGroupService: UserGroupService,
    private log: LoggerService,
  ) { }

  ngAfterViewInit(): void {
    this.fetchMembers().then();
  }

  async fetchMembers() {
    this.log.info('Fetching members for group', this.group.id);
    const observable = this.userGroupService.getGroupMembersCached(this.group.id);
    this.members = await firstValueFrom(observable);
    this.members = await lastValueFrom(observable);
  }
}
