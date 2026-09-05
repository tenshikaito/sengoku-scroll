/** Detaching display objects alone leaves their renderer resources awaiting GC. */
export function destroyLayerChildren(layer: {
  removeChildren(): Array<{ destroy(options: { children: boolean }): void }>;
}): void {
  for (const child of layer.removeChildren()) child.destroy({ children: true });
}
