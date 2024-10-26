import { Pipe, PipeTransform } from '@angular/core';

const siPrefixes = ['', 'k', 'M', 'G', 'T', 'P', 'E', 'Z', 'Y', 'R', 'Q'];

@Pipe({
  name: 'byteSize',
  standalone: true
})
export class ByteSizePipe implements PipeTransform {

  transform(value: number, base2: boolean = true): string {
    if (value < 0) {
      throw new Error('Negative numbers are not supported');
    }

    const multiplier = base2 ? 1024 : 1000;

    if (value < multiplier) {
      return `${value} B`;
    }

    const exponent = Math.floor(Math.log(value) / Math.log(multiplier));
    const valueStr = value / Math.pow(multiplier, exponent);

    return `${valueStr.toFixed(2)} ${siPrefixes[exponent]}B`;
  }

}
