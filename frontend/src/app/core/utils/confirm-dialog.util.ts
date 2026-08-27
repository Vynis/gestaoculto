export async function confirmarExclusao(mensagem: string): Promise<boolean> {
  return confirmarAcao('Confirmar exclusão', mensagem, 'warning', 'Sim, excluir', '#c62828');
}

export async function confirmarAcao(
  titulo: string,
  mensagem: string,
  icon: 'warning' | 'question' | 'info' | 'error' | 'success' = 'question',
  confirmButtonText = 'Confirmar',
  confirmButtonColor = '#1976d2'
): Promise<boolean> {
  const Swal = (await import('sweetalert2')).default;

  const result = await Swal.fire({
    title: titulo,
    text: mensagem,
    icon,
    showCancelButton: true,
    confirmButtonText,
    cancelButtonText: 'Cancelar',
    confirmButtonColor,
    reverseButtons: true,
    focusCancel: true
  });

  return !!result.isConfirmed;
}
