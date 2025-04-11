import { InterpolationParameters, TranslateService, Translation, TranslationObject } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

export async function getTranslation(
  translateService: TranslateService,
  key: string,
  interpolateParams?: InterpolationParameters
): Promise<string> {
  const value = await firstValueFrom(translateService.get(key, interpolateParams)) as Promise<Translation | TranslationObject>;

  // If the translation is not a string, throw an error
  if (typeof value !== 'string') {
    throw new Error(`Translation for key "${key}" is not a string`);
  }

  // Otherwise, return the translation
  return value;
}

const pluralPoints = {
  'en': [1], // English uses 1 for singular, plural otherwise
  'hr': [1, 4], // Croatian uses 1 for singular, 2-4 for few, multiple otherwise
}

export async function getPluralTranslation(
  translateService: TranslateService,
  key: string,
  count: number,
  interpolateParams?: InterpolationParameters
): Promise<string> {
  const lang = translateService.currentLang;
  let points = pluralPoints['en'];
  if (lang === 'hr') {
    points = pluralPoints['hr'];
  }

  const translation = await getTranslation(translateService, key, interpolateParams);
  const split = translation.split('|').map((s) => s.trim());

  // Pluralization logic for Croatian
  if (lang === 'hr') {
    if (count !== 0 && count % 100 === 0) {
      count = 10;
    }
    else {
      count = count % 100;
      if (count % 100 > 20) {
        count = count % 10;
      }
    }
  }

  for (let i = 0; i < points.length; i++) {
    if (count <= points[i]) {
      const value = split[i];
      if (!value) {
        throw new Error(`Missing translation for plural point ${points[i]} in key "${key}"`);
      }
      return value;
    }
  }

  const pointsLength = split.length;
  const lastValue = split[pointsLength - 1];
  if (!lastValue) {
    throw new Error(`Missing translation for plural point ${points[pointsLength - 1]} in key "${key}"`);
  }

  return lastValue;
}
