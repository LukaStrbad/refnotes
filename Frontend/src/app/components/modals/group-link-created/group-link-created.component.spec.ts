import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GroupLinkCreatedComponent } from './group-link-created.component';

describe('GroupLinkCreatedComponent', () => {
  let component: GroupLinkCreatedComponent;
  let fixture: ComponentFixture<GroupLinkCreatedComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupLinkCreatedComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GroupLinkCreatedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
