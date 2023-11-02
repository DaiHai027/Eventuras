import { PaymentProvider } from '@losol/eventuras';
import useTranslation from 'next-translate/useTranslation';
import { useEffect, useState } from 'react';
import { SubmitHandler, useForm } from 'react-hook-form';

import { defaultInputStyle, InputText } from '@/components/forms/Input';
import Button from '@/components/ui/Button';
import Container from '@/components/ui/Container';
import Heading from '@/components/ui/Heading';
import PaymentFormValues from '@/types/PaymentFormValues';
import { UserProfile } from '@/types/UserProfile';

export type RegistrationPaymentProps = {
  onSubmit: (values: PaymentFormValues) => void;
  userProfile: UserProfile;
};

const RegistrationPayment = ({ userProfile, onSubmit }: RegistrationPaymentProps) => {
  const { t } = useTranslation('register');
  const {
    register,
    formState: { errors },
    handleSubmit,
    watch,
  } = useForm<PaymentFormValues>();

  const [showBusinessFieldset, setShowBusinessFieldset] = useState(false);

  const selectedPaymentMethod = watch('paymentMethod');

  useEffect(() => {
    // Use useEffect to update showBusinessFieldset after initial render
    if (selectedPaymentMethod === PaymentProvider.POWER_OFFICE_EHFINVOICE) {
      setShowBusinessFieldset(true);
    } else {
      setShowBusinessFieldset(false);
    }
  }, [selectedPaymentMethod]);

  const onSubmitForm: SubmitHandler<PaymentFormValues> = (data: PaymentFormValues) => {
    onSubmit(data);
  };

  const formClassName = 'px-8 pt-6 pb-8 mb-4';
  const fieldsetClassName = 'text-lg pt-3 pb-6';
  const fieldsetLegendClassName = 'text-lg border-b-2 pt-4 pb-2';

  return (
    <>
      <Container>
        <Heading>{t('registration:payment.title')}</Heading>
        <p>{t('registration:payment.description')}</p>
        <form onSubmit={handleSubmit(onSubmitForm)} className={formClassName}>
          <fieldset className={fieldsetClassName}>
            <legend className={fieldsetLegendClassName}>
              {t('registration:form.customertype.legend')}
            </legend>
            <ul className="flex flex-col">
              <li>
                <input
                  type="radio"
                  id="emailinvoice"
                  value={PaymentProvider.POWER_OFFICE_EMAIL_INVOICE}
                  defaultChecked={true}
                  {...register('paymentMethod')}
                />
                <label htmlFor="emailinvoice">{t('registration:form.customertype.private')}</label>
              </li>
              <li>
                <input
                  type="radio"
                  id="ehfInvoice"
                  value={PaymentProvider.POWER_OFFICE_EHFINVOICE}
                  {...register('paymentMethod')}
                />
                <label htmlFor="ehfInvoice">{t('registration:form.customertype.business')}</label>
              </li>
            </ul>
          </fieldset>

          <fieldset className={fieldsetClassName}>
            <legend className={fieldsetLegendClassName}>
              {t('registration:form.user.legend')}
            </legend>
            <InputText
              {...register('username', { value: userProfile.name })}
              label={t('registration:form.user.name')}
              defaultValue={userProfile.name}
              disabled
              errors={errors}
              className={defaultInputStyle}
            />
            <InputText
              {...register('email', { value: userProfile.email })}
              label={t('registration:form.user.email')}
              defaultValue={userProfile.email}
              disabled
              errors={errors}
              className={defaultInputStyle}
            />
          </fieldset>
          <fieldset className={fieldsetClassName}>
            <legend className={fieldsetLegendClassName}>
              {t('registration:form.address.legend')}
            </legend>
            <InputText
              {...register('zip', {
                required: 'Zip code is Required',
              })}
              label={t('registration:form.address.zip')}
              placeholder="Zip Code"
              errors={errors}
              className={defaultInputStyle}
            />
            <InputText
              {...register('city', {
                required: 'City is required',
              })}
              label={t('registration:form.address.city')}
              placeholder="City"
              errors={errors}
              className={defaultInputStyle}
            />
            <InputText
              {...register('country', {
                required: 'Country is required',
              })}
              label={t('registration:form.address.country')}
              default="Norway"
              placeholder="Country"
              errors={errors}
              className={defaultInputStyle}
            />
          </fieldset>

          {showBusinessFieldset && (
            <fieldset className={fieldsetClassName}>
              <legend className={fieldsetLegendClassName}>
                {t('registration:form.businessinfo.legend')}
              </legend>
              <InputText
                {...register('vatNumber', {
                  required: 'Vat Number is required for business customers',
                })}
                label={t('registration:form.businessinfo.vatNumber')}
                placeholder="Vat Number"
                errors={errors}
                className={defaultInputStyle}
              />
              <InputText
                {...register('invoiceReference')}
                label={t('registration:form.businessinfo.invoiceReference')}
                placeholder="Invoice Reference"
                errors={errors}
                className={defaultInputStyle}
              />
            </fieldset>
          )}

          <Button type="submit">{t('common:buttons.continue')}</Button>
        </form>
      </Container>
    </>
  );
};

export default RegistrationPayment;
