import { TestBed } from "@angular/core/testing";
import { ActivatedRoute } from "@angular/router";
import { joinPaths } from "../utils/path-utils";

export function mockActivatedRoute(directory: string, file: string) {
  const activatedRoute = TestBed.inject(ActivatedRoute);
  activatedRoute.snapshot.paramMap.get = (param: string) => {
    if (param === 'path') {
      return joinPaths(directory, file);
    }

    throw new Error(`Unknown query parameter: ${param}`);
  }
}
