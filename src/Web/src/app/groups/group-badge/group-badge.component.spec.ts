import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupBadgeComponent } from './group-badge.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { UserGroupRole } from '../../../model/user-group';

describe('GroupBadgeComponent', () => {
  let component: GroupBadgeComponent;
  let fixture: ComponentFixture<GroupBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupBadgeComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        })
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupBadgeComponent);
    component = fixture.componentInstance;
    component.role = UserGroupRole.Owner;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
