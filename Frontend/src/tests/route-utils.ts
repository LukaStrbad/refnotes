import { TestBed } from "@angular/core/testing";
import { ActivatedRoute } from "@angular/router";

export function mockActivatedRoute(values: Record<string, string>) {
  const activatedRoute = TestBed.inject(ActivatedRoute);
  activatedRoute.snapshot.paramMap.get = (param: string) => {
    if (param in values) {
      return values[param];
    }
    return null;
  };
}
