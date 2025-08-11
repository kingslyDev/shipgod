// User management scripts (scoped)
(function () {
  const root = document.getElementById('userPage');
  if (!root) return;
  root.querySelectorAll('#togglePwd').forEach((btn) => {
    btn.addEventListener('click', function () {
      const pwd = root.querySelector('#Password');
      if (!pwd) return;
      const icon = this.querySelector('i');
      if (pwd.type === 'password') {
        pwd.type = 'text';
        icon?.classList.replace('fa-eye', 'fa-eye-slash');
      } else {
        pwd.type = 'password';
        icon?.classList.replace('fa-eye-slash', 'fa-eye');
      }
    });
  });
})();
