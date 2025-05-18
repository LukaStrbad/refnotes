import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GroupsComponent } from './groups.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { UserGroupService } from '../../services/user-group.service';
import { NotificationService } from '../../services/notification.service';
import { LoggerService } from '../../services/logger.service';
import { GroupDto, UserGroupRole } from '../../model/user-group';
import { of } from 'rxjs';
import { By } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';

describe('GroupsComponent', () => {
  let component: GroupsComponent;
  let fixture: ComponentFixture<GroupsComponent>;
  let userGroupService: jasmine.SpyObj<UserGroupService>;
  let notificationService: jasmine.SpyObj<NotificationService>;
  let nativeElement: HTMLElement;

  const mockGroups: GroupDto[] = [
    { id: 1, name: 'Group 1', role: UserGroupRole.Owner },
    { id: 2, name: 'Group 2', role: UserGroupRole.Member }
  ];

  beforeEach(async () => {
    userGroupService = jasmine.createSpyObj('UserGroupService', ['getUserGroupsCached', 'create', 'generateAccessCode']);
    notificationService = jasmine.createSpyObj('NotificationService', ['awaitAndNotifyError']);

    // Setup default spy behavior
    userGroupService.getUserGroupsCached.and.returnValue(of(mockGroups));
    userGroupService.create.and.callFake(async (name) => Promise.resolve({
      id: 3,
      name: name,
      role: UserGroupRole.Owner
    } as GroupDto));
    notificationService.awaitAndNotifyError.and.callFake(async (promise) => promise);

    await TestBed.configureTestingModule({
      imports: [
        GroupsComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader
          }
        })
      ],
      providers: [
        { provide: UserGroupService, useValue: userGroupService },
        { provide: NotificationService, useValue: notificationService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: {} } },
        },
        LoggerService,
        TranslateService
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupsComponent);
    component = fixture.componentInstance;
    nativeElement = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load groups on init', async () => {
    await fixture.whenStable();
    expect(userGroupService.getUserGroupsCached).toHaveBeenCalled();
    expect(component.groups).toEqual(mockGroups);
  });

  it('should add new group when created', async () => {
    const newGroupName = 'New Group';
    await component.onGroupCreated(newGroupName);

    expect(userGroupService.create).toHaveBeenCalledWith(newGroupName);
    expect(component.groups).toContain(jasmine.objectContaining({
      name: newGroupName,
      role: UserGroupRole.Owner
    }));
  });

  it('should generate invite link for a group', async () => {
    const mockAccessCode = 'abc123';
    const group = mockGroups[0];
    const expectedLink = `${window.location.origin}/join-group/${group.id}/${encodeURIComponent(mockAccessCode)}`;

    userGroupService.generateAccessCode.and.resolveTo(mockAccessCode);

    const groupLinkCreatedModalSpy = spyOn(component.groupLinkCreatedModal, 'show');

    await component.createInviteLink(group);

    expect(userGroupService.generateAccessCode).toHaveBeenCalledWith(group.id);
    expect(groupLinkCreatedModalSpy).toHaveBeenCalledWith(expectedLink);
  });

  it('should show no groups message when groups array is empty', () => {
    component.groups = [];
    fixture.detectChanges();

    const alertElement = nativeElement.querySelector('[data-test="groups.no-groups"]');
    expect(alertElement).toBeTruthy();

  });

  it('should display all groups as cards', async () => {
    await fixture.whenStable();
    fixture.detectChanges();

    const groupCards = fixture.debugElement.queryAll(By.css('[data-test="groups.group-card"]'));
    expect(groupCards.length).toBe(mockGroups.length);

    groupCards.forEach((card, index) => {
      expect(card.componentInstance.group).toEqual(mockGroups[index]);
    });
  });
});
