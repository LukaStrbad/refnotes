import { ActivatedRouteSnapshot, BaseRouteReuseStrategy } from "@angular/router";

export class CustomRouteReuseStrategy extends BaseRouteReuseStrategy {
  override shouldReuseRoute(future: ActivatedRouteSnapshot, curr: ActivatedRouteSnapshot): boolean {
    // Disable for /preview and /editor routes
    const path = future.routeConfig?.path;
    if (path === 'preview' || path === 'editor') {
      return false;
    }

    return future.routeConfig === curr.routeConfig;
  }
}
