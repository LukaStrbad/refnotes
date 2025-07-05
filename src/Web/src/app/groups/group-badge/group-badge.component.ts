import { Component, Input } from '@angular/core';
import { UserGroupRole } from '../../../model/user-group';
import { TranslatePipe } from '@ngx-translate/core';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-group-badge',
  imports: [TranslatePipe, NgClass],
  templateUrl: './group-badge.component.html',
  styleUrl: './group-badge.component.css'
})
export class GroupBadgeComponent {
  readonly UserGroupRole = UserGroupRole; // Make enum available in template

  @Input({ required: true })
  role!: UserGroupRole;
}
