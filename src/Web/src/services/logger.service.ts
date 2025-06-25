import { Injectable } from '@angular/core';
import { environment } from "../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class LoggerService {

  currentTimestamp() {
    // Format: YYYY-MM-DD HH:MM:SS
    return new Date().toISOString().replace('T', ' ').replace('Z', '');
  }

  private static isProduction() {
    return environment.production;
  }

  info(message: string, ...args: ArgType[]) {
    if (LoggerService.isProduction()) return;
    console.log(`[${this.currentTimestamp()}] [INFO] ${message}`, ...args);
  }

  warn(message: string, ...args: ArgType[]) {
    if (LoggerService.isProduction()) return;
    console.warn(`[${this.currentTimestamp()}] [WARN] ${message}`, ...args);
  }

  error(message: string, ...args: ArgType[]) {
    if (LoggerService.isProduction()) return;
    console.error(`[${this.currentTimestamp()}] [ERROR] ${message}`, ...args);
  }
}

type ArgType = string | number | boolean | object | unknown | null | undefined;
