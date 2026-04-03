export async function confirmarExclusao(mensagem: string): Promise<boolean> {
  const Swal = (await import('sweetalert2')).default;

  const result = await Swal.fire({
    title: 'Confirmar exclusão',
    text: mensagem,
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Sim, excluir',
    cancelButtonText: 'Cancelar',
    confirmButtonColor: '#c62828',
    reverseButtons: true,
    focusCancel: true
  });

  return !!result.isConfirmed;
}
