import { HttpParams } from "@angular/common/http";

export function generateHttpParams(params: Record<string, string | number | boolean | undefined | null>): HttpParams {
  let httpParams = new HttpParams();

  for (const key in params) {
    const value = params[key];
    // Skip undefined or null values
    if (value === undefined || value === null) {
      continue;
    }

    httpParams = httpParams.set(key, value);
  }

  return httpParams;
}
