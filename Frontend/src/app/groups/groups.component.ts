import { Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { GroupDto } from '../../model/user-group';
import { GroupCardComponent } from "./group-card/group-card.component";
import { CreateGroupModalComponent } from "../components/modals/create-group-modal/create-group-modal.component";
import { UserGroupService } from '../../services/user-group.service';
import { firstValueFrom, lastValueFrom } from 'rxjs';

@Component({
  selector: 'app-groups',
  imports: [TranslatePipe, GroupCardComponent, CreateGroupModalComponent],
  templateUrl: './groups.component.html',
  styleUrl: './groups.component.css'
})
export class GroupsComponent {
  groups: GroupDto[] = [];

  constructor(
    private userGroupService: UserGroupService,
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
}
