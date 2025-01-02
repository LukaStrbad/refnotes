import { Route, Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { HomeComponent } from './home/home.component';
import { FileEditorComponent } from './file-editor/file-editor.component';
import { SettingsComponent } from './settings/settings.component';

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
        path: "editor",
        component: FileEditorComponent
    },
    {
        path: "browser",
        component: HomeComponent,
    },
    {
        path: "settings",
        component: SettingsComponent,
    }
];
