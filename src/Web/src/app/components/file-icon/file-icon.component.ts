import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { getFileType } from '../../../utils/file-utils';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-file-icon',
  imports: [CommonModule],
  templateUrl: './file-icon.component.html',
  styleUrl: './file-icon.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileIconComponent {
  readonly fileName = input.required<string>();

  readonly fileType = computed(() => getFileType(this.fileName()));
}
