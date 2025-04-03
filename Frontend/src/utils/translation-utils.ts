import { TranslateService, Translation, TranslationObject } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

export async function getTranslation(translateService: TranslateService, key: string): Promise<string> {
  const value = await firstValueFrom(translateService.get(key)) as Promise<Translation | TranslationObject>;

  // If the translation is not a string, throw an error
  if (typeof value !== 'string') {
    throw new Error(`Translation for key "${key}" is not a string`);
  }

  // Otherwise, return the translation
  return value;
}
