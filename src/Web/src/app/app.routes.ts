import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { SettingsComponent } from './settings/settings.component';
import { BrowserComponent } from './browser/browser.component';
import { RegisterComponent } from './register/register.component';
import { FileEditorComponent } from './file-editor/file-editor.component';
import { FilePreviewComponent } from './file-preview/file-preview.component';
import { GroupsComponent } from './groups/groups.component';
import { GroupMembersListComponent } from './groups/group-members-list/group-members-list.component';
import { authGuard } from '../../guards/auth.guard';

export const routes: Routes = [
  {
    path: "",
    redirectTo: "browser",
    pathMatch: "full",
  },
  {
    path: "login",
    component: LoginComponent,
  },
  {
    path: "signup",
    component: RegisterComponent,
  },
  {
    path: "file/:path/edit",
    component: FileEditorComponent,
    canActivate: [authGuard],
  },
  {
    path: "file/:path/preview",
    component: FilePreviewComponent,
    canActivate: [authGuard],
  },
  {
    path: "file/public/:publicHash",
    component: FilePreviewComponent,
  },
  {
    path: "browser",
    component: BrowserComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '**',
        component: BrowserComponent,
        canActivate: [authGuard],
      }
    ]
  },
  {
    path: "settings",
    component: SettingsComponent,
    canActivate: [authGuard],
  },
  {
    path: "groups",
    component: GroupsComponent,
    canActivate: [authGuard],
  },
  {
    path: "groups/:id/members",
    component: GroupMembersListComponent,
    canActivate: [authGuard],
  },
  {
    path: "groups/:groupId/browser",
    component: BrowserComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '**',
        component: BrowserComponent,
        canActivate: [authGuard],
      }
    ]
  },
  {
    path: "groups/:groupId/file/:path/edit",
    component: FileEditorComponent,
    canActivate: [authGuard],
  },
  {
    path: "groups/:groupId/file/:path/preview",
    component: FilePreviewComponent,
    canActivate: [authGuard],
  },
  {
    path: "join-group/:id/:code",
    component: GroupsComponent,
    canActivate: [authGuard],
  }
];
