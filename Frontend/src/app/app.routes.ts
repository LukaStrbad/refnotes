import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { FileEditorComponent } from './file-editor/file-editor.component';
import { SettingsComponent } from './settings/settings.component';
import { BrowserComponent } from './browser/browser.component';
import { RegisterComponent } from './register/register.component';

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
        path: "editor",
        component: FileEditorComponent
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
    }
];
