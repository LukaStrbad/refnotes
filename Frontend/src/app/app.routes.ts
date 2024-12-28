import { Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { HomeComponent } from './home/home.component';
import { FileEditorComponent } from './file-editor/file-editor.component';

export const headerRoutes: Routes = [
    {
        path: "homepage",
        component: HomeComponent,
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
