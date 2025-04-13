import { TranslateService } from "@ngx-translate/core";
import { getPluralTranslation } from "./translation-utils";
import { of } from "rxjs";

describe('Translation utils', () => {
  let translateService: jasmine.SpyObj<TranslateService>;

  beforeEach(() => {
    translateService = jasmine.createSpyObj('TranslateService', ['get']);
  });

  [
    [1, '1 number'],
    [2, '2 numbers'],
    [10, '10 numbers'],
  ].forEach(([count, expected]) => {
    it(`getPluralTranslation (en) should return "${expected}" for count ${count}`, async () => {
      const translation = '{{n}} number | {{n}} numbers'.replaceAll('{{n}}', count.toString());
      translateService.get.and.returnValue(of(translation));
      translateService.currentLang = 'en';

      const countNum = count as number;

      const result = await getPluralTranslation(translateService, 'test.key', countNum, { n: countNum });
      expect(result).toBe(expected.toString());
    });
  });

  [
    [1, '1 broj'],
    [2, '2 broja'],
    [3, '3 broja'],
    [4, '4 broja'],
    [5, '5 brojeva'],
    [10, '10 brojeva'],
    [11, '11 brojeva'],
    [12, '12 brojeva'],
    [20, '20 brojeva'],
    [21, '21 broj'],
    [22, '22 broja'],
    [23, '23 broja'],
    [24, '24 broja'],
    [25, '25 brojeva'],
    [100, '100 brojeva'],
    [101, '101 broj'],
    [102, '102 broja'],
    [103, '103 broja'],
    [104, '104 broja'],
    [105, '105 brojeva'],
  ].forEach(([count, expected]) => {
    it(`getPluralTranslation (hr) should return "${expected}" for count ${count}`, async () => {
      const translation = '{{n}} broj | {{n}} broja | {{n}} brojeva'.replaceAll('{{n}}', count.toString());
      translateService.get.and.returnValue(of(translation));
      translateService.currentLang = 'hr';

      const countNum = count as number;

      const result = await getPluralTranslation(translateService, 'test.key', countNum, { n: countNum });
      expect(result).toBe(expected.toString());
    });
  });
});
