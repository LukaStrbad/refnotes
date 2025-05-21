import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GroupCardComponent } from './group-card.component';
import { UserGroupService } from '../../../services/user-group.service';
import { LoggerService } from '../../../services/logger.service';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { GroupUserDto, UserGroupRole } from '../../../model/user-group';
import { of } from 'rxjs';
import { ActivatedRoute } from '@angular/router';

describe('GroupCardComponent', () => {
  let component: GroupCardComponent;
  let fixture: ComponentFixture<GroupCardComponent>;
  let userGroupService: jasmine.SpyObj<UserGroupService>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    userGroupService = jasmine.createSpyObj('UserGroupService', ['getGroupMembersCached']);

    // Mock behavior for getGroupMembersCached
    userGroupService.getGroupMembersCached.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [GroupCardComponent, TranslatePipe,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        })
      ],
      providers: [
        { provide: UserGroupService, useValue: userGroupService },
        LoggerService,
        TranslateService,
        { provide: ActivatedRoute, useValue: {} },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupCardComponent);
    component = fixture.componentInstance;

    // Set required input
    component.group = {
      id: 1,
      name: 'Test Group',
      role: UserGroupRole.Member
    };

    fixture.detectChanges();
    nativeElement = fixture.nativeElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should fetch members on init', async () => {
    const members: GroupUserDto[] = [
      { id: 1, username: 'user1', role: UserGroupRole.Member },
      { id: 2, username: 'user2', role: UserGroupRole.Admin }
    ];
    userGroupService.getGroupMembersCached.and.returnValue(of(members));

    component.ngAfterViewInit();
    await fixture.whenStable();

    expect(userGroupService.getGroupMembersCached).toHaveBeenCalledWith(1);
    expect(component.members).toEqual(members);
  });

  it('should allow invites for Owner role', () => {
    component.group.role = UserGroupRole.Owner;
    expect(component.canInvite()).toBeTruthy();
  });

  it('should allow invites for Admin role', () => {
    component.group.role = UserGroupRole.Admin;
    expect(component.canInvite()).toBeTruthy();
  });

  it('should not allow invites for Member role', () => {
    component.group.role = UserGroupRole.Member;
    expect(component.canInvite()).toBeFalsy();
  });

  it('should allow editing for Owner and Admin roles', () => {
    component.group.role = UserGroupRole.Owner;
    expect(component.canEdit()).toBeTruthy();

    component.group.role = UserGroupRole.Admin;
    expect(component.canEdit()).toBeTruthy();

    // Members cannot edit
    component.group.role = UserGroupRole.Member;
    expect(component.canEdit()).toBeFalsy();
  });

  it('should emit invite event when invite button is clicked', () => {
    spyOn(component.invite, 'emit');
    component.group.role = UserGroupRole.Owner; // Set role to Owner to enable invite button
    component.canInviteMembers = true;
    fixture.detectChanges();

    const inviteButton = nativeElement.querySelector('button[data-test="groups.card.invite"]') as HTMLButtonElement;
    expect(inviteButton).toBeTruthy();
    inviteButton.click();

    expect(component.invite.emit).toHaveBeenCalledWith(component.group);
  });

  it('should not show invite button for regular members', () => {
    component.group.role = UserGroupRole.Member;
    component.canInviteMembers = false;
    fixture.detectChanges();

    const inviteButton = nativeElement.querySelector('button[data-test="groups.card.invite"]');
    expect(inviteButton).toBeNull();
  });

  it('should show loading state while fetching members', () => {
    component.members = null;
    fixture.detectChanges();

    const loadingElement = nativeElement.querySelector('.loading');
    expect(loadingElement).toBeTruthy();
  });
});
