import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class CookieService {

  setCookie(name: string, value: string, expires: Date) {
    const expiresString = `expires=${expires.toUTCString()}`;
    document.cookie = `${name}=${value}; path=/; ${expiresString}`;
  }

  getCookie(name: string): string | null {
    const nameEQ = `${name}=`;
    const split = document.cookie.split(';');
    for (const cookieItem of split) {
      let c = cookieItem;
      while (c.charAt(0) === ' ') c = c.substring(1, c.length);
      if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
  }

}
