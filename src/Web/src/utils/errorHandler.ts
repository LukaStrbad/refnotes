import { HttpErrorResponse } from "@angular/common/http";

export interface HttpErrorMessages {
  [key: number]: string;
  default?: string;
};

export const GENERIC_ERROR_CODE = "generic.error";

export function getErrorMessage(e: unknown, messages: HttpErrorMessages): string {
  if (e instanceof HttpErrorResponse) {
    for (const [status, message] of Object.entries(messages)) {
      if (e.status === Number(status)) {
        return message;
      }
    }
  }

  if (messages.default) {
    return messages.default;
  }

  return GENERIC_ERROR_CODE;
}

export function getStatusCode(e: unknown): number | null {
  if (e instanceof HttpErrorResponse) {
    return e.status;
  }

  return null;
}
