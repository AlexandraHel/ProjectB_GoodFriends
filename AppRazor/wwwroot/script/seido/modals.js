//Just to ensure we force js into strict mode in HTML scrips - we don't want any sloppy code
'use strict';  


//Script to set title and body of modals
function launchModal (event)  {

    //Extract info from data-* attributes
    var btn = event.relatedTarget;
    var mod_title = btn.dataset.seidoModalTitle;
    var mod_body = btn.dataset.seidoModalBody;
    var mod_cancel = btn.dataset.seidoModalCancel;
    var mod_ok = btn.dataset.seidoModalOk;

    //Set the attributes into the modal
    var modal = event.currentTarget;
    var title = modal.querySelector('.modal-title');
    var body = modal.querySelector('.modal-body');
    var btn_cancel = modal.querySelector('.btn-secondary');
    var btn_ok = modal.querySelector('.btn-primary');

    title.textContent = mod_title ?? title.textContent;
    body.textContent = mod_body ?? body.textContent;
    btn_cancel.textContent = mod_cancel ?? btn_cancel.textContent;
    btn_ok.textContent = mod_ok ?? btn_ok.textContent;        
  };


  //Add launchHandler to all you modals
  /*
  document.getElementById('softModal').addEventListener('show.bs.modal', launchModal);
  document.getElementById('hardModal').addEventListener('show.bs.modal', launchModal);
  document.getElementById('dangerModal').addEventListener('show.bs.modal', launchModal);
*/

let melems = document.querySelectorAll('.modal.fade');
melems.forEach(elem => {
  elem.addEventListener('show.bs.modal', launchModal);
});
