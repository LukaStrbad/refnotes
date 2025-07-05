import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeButtonComponent } from './home-button.component';
import { AuthService } from '../../../services/auth.service';
import { ActivatedRoute } from '@angular/router';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';

describe('HomeButtonComponent', () => {
  let component: HomeButtonComponent;
  let fixture: ComponentFixture<HomeButtonComponent>;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj('AuthService', [], {
      isUserLoggedIn: () => true,
    });

    await TestBed.configureTestingModule({
      imports: [HomeButtonComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        { provide: AuthService, useValue: authService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: {} } },
        },
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(HomeButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
