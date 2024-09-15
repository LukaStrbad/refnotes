import { HttpErrorResponse } from "@angular/common/http";

interface Messages {
    [key: number]: string;
}

export function getErrorMessage(e: unknown, messages: Messages): string {
    if (e instanceof HttpErrorResponse) {
        for (const [status, message] of Object.entries(messages)) {
            if (e.status === Number(status)) {
                return message;
            }
        }
    }

    console.error(e);
    return "An error occurred";
}

export function getStatusCode(e: unknown): number | null {
    if (e instanceof HttpErrorResponse) {
        return e.status;
    }

    return null;
}
