import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { SettingsComponent } from './settings/settings.component';
import { BrowserComponent } from './browser/browser.component';
import { RegisterComponent } from './register/register.component';
import { FileEditorComponent } from './file-editor/file-editor.component';
import { FilePreviewComponent } from './file-preview/file-preview.component';
import { GroupsComponent } from './groups/groups.component';

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
  },
  {
    path: "file/:path/preview",
    component: FilePreviewComponent,
  },
  {
    path: "browser",
    component: BrowserComponent,
    children: [
      {
        path: '**',
        component: BrowserComponent
      }
    ]
  },
  {
    path: "settings",
    component: SettingsComponent,
  },
  {
    path: "groups",
    component: GroupsComponent,
  },
];
