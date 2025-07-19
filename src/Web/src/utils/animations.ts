import { animate, AnimationTriggerMetadata, style, transition, trigger } from "@angular/animations";

export function getRevealAnimations(): AnimationTriggerMetadata {
    return trigger('reveal', [
        transition(':enter', [
            style({
                opacity: 0,
                transform: 'translateY(-5px) scale(0.95)'
            }),
            animate('120ms ease-out', style({
                opacity: 1,
                transform: 'translateY(0px) scale(1)'
            }))
        ]),
        transition(':leave', [
            animate('120ms ease-in', style({
                opacity: 0,
                transform: 'translateY(-5px) scale(0.95)'
            }))
        ])
    ]);
}
