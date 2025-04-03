import { HttpErrorResponse } from "@angular/common/http";

export interface HttpErrorMessages {
    [key: number]: string;
}

export function getErrorMessage(e: unknown, messages: HttpErrorMessages): string {
    if (e instanceof HttpErrorResponse) {
        for (const [status, message] of Object.entries(messages)) {
            if (e.status === Number(status)) {
                return message;
            }
        }
    }

    return "generic.error";
}

export function getStatusCode(e: unknown): number | null {
    if (e instanceof HttpErrorResponse) {
        return e.status;
    }

    return null;
}
