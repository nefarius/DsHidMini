#include <check.h>
#include <stdlib.h>
#include <string.h>
#include <float.h>

#include "driver/JSON/cJSON.c"

START_TEST(test_number_print_no_overflow)
{
    /* Invariant: printing a cJSON number must never overflow internal buffers,
       regardless of the double value provided. */
    double payloads[] = {
        DBL_MAX,                    /* extreme: longest %1.17g output */
        -DBL_MAX,                   /* extreme negative */
        1.7976931348623157e+308,    /* boundary: max finite double */
        0.0,                        /* valid simple case */
        1.23456789012345678e-308    /* near-denormal, long representation */
    };
    int num_payloads = sizeof(payloads) / sizeof(payloads[0]);

    for (int i = 0; i < num_payloads; i++) {
        cJSON *item = cJSON_CreateNumber(payloads[i]);
        ck_assert_ptr_nonnull(item);

        char *printed = cJSON_Print(item);
        ck_assert_ptr_nonnull(printed);
        /* Output must be a reasonable length (sprintf %1.17g max ~24 chars + sign + null) */
        ck_assert_uint_lt(strlen(printed), 64);

        free(printed);
        cJSON_Delete(item);
    }
}
END_TEST

Suite *security_suite(void)
{
    Suite *s;
    TCase *tc_core;

    s = suite_create("Security");
    tc_core = tcase_create("Core");

    tcase_add_test(tc_core, test_number_print_no_overflow);
    suite_add_tcase(s, tc_core);

    return s;
}

int main(void)
{
    int number_failed;
    Suite *s;
    SRunner *sr;

    s = security_suite();
    sr = srunner_create(s);

    srunner_run_all(sr, CK_NORMAL);
    number_failed = srunner_ntests_failed(sr);
    srunner_free(sr);

    return (number_failed == 0) ? EXIT_SUCCESS : EXIT_FAILURE;
}