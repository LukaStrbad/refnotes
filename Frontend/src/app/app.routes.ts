import { Route, Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { HomeComponent } from './home/home.component';
import { FileEditorComponent } from './file-editor/file-editor.component';
import { SettingsComponent } from './settings/settings.component';

export const headerRoutes: RouteWithIcon[] = [
    {
        path: "homepage",
        component: HomeComponent,
        icon: "house",
    },
    {
        path: "settings",
        component: SettingsComponent,
        icon: "gear",
    }
];

export const routes: Routes = [
    {
        path: "",
        redirectTo: "homepage",
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
    ...headerRoutes
];

export interface RouteWithIcon extends Route {
    icon: string;
}
