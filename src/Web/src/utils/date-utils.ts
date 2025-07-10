import { TranslateService } from "@ngx-translate/core";
import { FileInfo, FileWithTime } from "../model/file";
import { getPluralTranslation, getTranslation } from "./translation-utils";

export function mapFileDates(file: FileInfo): FileInfo {
  file.modified = new Date(file.modified);
  file.created = new Date(file.created);
  return file;
}

export async function getFormattedDate(
  translateService: TranslateService,
  date: Date | string,
  lang: string,
  long = false
): Promise<string> {
  const now = new Date();

  if (long) {
    return date.toLocaleString(lang);
  }

  if (typeof date === 'string') {
    date = new Date(date);
  }

  const currentTime = now.getTime();
  const dateTime = date.getTime();

  // Check if it's less than 60 seconds ago
  if (currentTime - dateTime < 60 * 1000) {
    return await getTranslation(translateService, 'time.just-now');
  }

  // Check if it's less than 60 minutes ago
  if (currentTime - dateTime < 60 * 60 * 1000) {
    const minutes = Math.floor((currentTime - dateTime) / (60 * 1000));
    return await getPluralTranslation(translateService, 'time.minutes-ago', minutes, { n: minutes });
  }

  // Check if it's less than 24 hours ago
  if (currentTime - dateTime < 24 * 60 * 60 * 1000) {
    const hours = Math.floor((currentTime - dateTime) / (60 * 60 * 1000));
    return await getPluralTranslation(translateService, 'time.hours-ago', hours, { n: hours });
  }

  // Check if it's less than 7 days ago
  if (currentTime - dateTime < 7 * 24 * 60 * 60 * 1000) {
    const days = Math.floor((currentTime - dateTime) / (24 * 60 * 60 * 1000));
    return await getPluralTranslation(translateService, 'time.days-ago', days, { n: days });
  }

  // Otherwise, return the date in the format "dd/mm/yyyy"
  return date.toLocaleDateString(lang);
}

export async function updateFileTime(file: FileWithTime, translateService: TranslateService, lang: string): Promise<FileWithTime> {
  const modified = file.modified;
  file.createdLong = await getFormattedDate(translateService, file.created, lang, true);
  file.createdShort = await getFormattedDate(translateService, file.created, lang, false);
  file.modifiedLong = await getFormattedDate(translateService, modified, lang, true);
  file.modifiedShort = await getFormattedDate(translateService, modified, lang, false);
  return file;
}

export function convertDateLocale(original: string): string {
  if (original === 'en') {
    return 'en-UK';
  }

  return original;
}
