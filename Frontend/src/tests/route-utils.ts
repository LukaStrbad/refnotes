import { TestBed } from "@angular/core/testing";
import { ActivatedRoute } from "@angular/router";

export function mockActivatedRoute(directory: string, file: string) {
  const activatedRoute = TestBed.inject(ActivatedRoute);
  activatedRoute.snapshot.queryParamMap.get = (param: string) => {
    if (param === 'directory') {
      return directory;
    } else if (param === 'file') {
      return file;
    }
    throw new Error(`Unknown query parameter: ${param}`);
  }
}
