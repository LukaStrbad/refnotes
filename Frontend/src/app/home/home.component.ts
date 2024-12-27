import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { MdEditorComponent } from '../components/md-editor/md-editor.component';
import { BrowserComponent } from "../browser/browser.component";

@Component({
    selector: 'app-home',
    imports: [MdEditorComponent, BrowserComponent],
    templateUrl: './home.component.html',
    styleUrl: './home.component.scss'
})
export class HomeComponent {
  constructor(
    private auth: AuthService,
    private router: Router
  ) {

    if (this.auth.user === null) {
      this.router.navigate(['/login'])
    }
  }
}
